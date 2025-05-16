/**
 * 评价系统相关功能
 */

// 渲染星级评分
function renderStarRating(rating) {
    let starsHtml = '';
    for (let i = 1; i <= 5; i++) {
        if (i <= rating) {
            starsHtml += '<i class="fas fa-star checked"></i>';
        } else {
            starsHtml += '<i class="fas fa-star"></i>';
        }
    }
    return starsHtml;
}

// 格式化日期
function formatDate(dateString) {
    const date = new Date(dateString);
    return date.toLocaleDateString('zh-CN', { 
        year: 'numeric', 
        month: 'long', 
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
    });
}

// 加载产品评价
async function loadProductReviews(productId, containerId) {
    try {
        const container = document.getElementById(containerId);
        if (!container) return;
        
        container.innerHTML = '<div class="text-center"><div class="spinner-border text-success" role="status"><span class="visually-hidden">加载中...</span></div></div>';
        
        const response = await api.reviews.getByProduct(productId);
        const reviews = response.data || [];
        
        if (reviews.length === 0) {
            container.innerHTML = '<div class="alert alert-info">暂无评价，快来写第一条评价吧！</div>';
            return;
        }
        
        let reviewsHtml = '';
        
        reviews.forEach(review => {
            reviewsHtml += `
                <div class="review-item">
                    <div class="d-flex justify-content-between align-items-center mb-2">
                        <div>
                            <span class="fw-bold">${review.isAnonymous ? '匿名用户' : review.userName}</span>
                            <span class="text-muted ms-2">购买于 ${formatDate(review.createdTime)}</span>
                        </div>
                        <div class="star-rating">${renderStarRating(review.rating)}</div>
                    </div>
                    <p>${review.content}</p>
                </div>
            `;
        });
        
        container.innerHTML = reviewsHtml;
    } catch (error) {
        console.error('加载评价失败:', error);
        document.getElementById(containerId).innerHTML = `<div class="alert alert-danger">加载评价失败: ${error.message}</div>`;
    }
}

// 加载我的评价
async function loadMyReviews(containerId) {
    try {
        const container = document.getElementById(containerId);
        if (!container) return;
        
        container.innerHTML = '<div class="text-center"><div class="spinner-border text-success" role="status"><span class="visually-hidden">加载中...</span></div></div>';
        
        const response = await api.reviews.getByUser();
        const reviews = response.data || [];
        
        if (reviews.length === 0) {
            container.innerHTML = '<div class="alert alert-info">您还没有发表过评价。</div>';
            return;
        }
        
        let reviewsHtml = '<div class="list-group">';
        
        reviews.forEach(review => {
            reviewsHtml += `
                <div class="list-group-item">
                    <div class="d-flex justify-content-between align-items-center mb-2">
                        <h5 class="mb-0">${review.productName}</h5>
                        <div class="star-rating">${renderStarRating(review.rating)}</div>
                    </div>
                    <p>${review.content}</p>
                    <div class="d-flex justify-content-between align-items-center">
                        <small class="text-muted">评价于 ${formatDate(review.createdTime)}</small>
                        <div>
                            <button class="btn btn-sm btn-outline-primary edit-review" data-review-id="${review.reviewId}">编辑</button>
                            <button class="btn btn-sm btn-outline-danger delete-review" data-review-id="${review.reviewId}">删除</button>
                        </div>
                    </div>
                </div>
            `;
        });
        
        reviewsHtml += '</div>';
        container.innerHTML = reviewsHtml;
        
        // 添加编辑和删除评价的事件监听
        document.querySelectorAll('.edit-review').forEach(button => {
            button.addEventListener('click', function() {
                const reviewId = this.getAttribute('data-review-id');
                showEditReviewModal(reviewId);
            });
        });
        
        document.querySelectorAll('.delete-review').forEach(button => {
            button.addEventListener('click', function() {
                const reviewId = this.getAttribute('data-review-id');
                if (confirm('确定要删除这条评价吗？')) {
                    deleteReview(reviewId);
                }
            });
        });
    } catch (error) {
        console.error('加载我的评价失败:', error);
        document.getElementById(containerId).innerHTML = `<div class="alert alert-danger">加载评价失败: ${error.message}</div>`;
    }
}

// 提交评价
async function submitReview(formId, productId) {
    try {
        const form = document.getElementById(formId);
        if (!form) return;
        
        const rating = parseInt(form.querySelector('input[name="rating"]:checked').value);
        const content = form.querySelector('textarea[name="content"]').value.trim();
        const isAnonymous = form.querySelector('input[name="isAnonymous"]').checked;
        
        if (!rating || !content) {
            alert('请输入评分和评价内容');
            return;
        }
        
        const reviewData = {
            productId,
            rating,
            content,
            isAnonymous
        };
        
        const submitButton = form.querySelector('button[type="submit"]');
        const originalText = submitButton.innerHTML;
        submitButton.disabled = true;
        submitButton.innerHTML = '<span class="spinner-border spinner-border-sm" role="status" aria-hidden="true"></span> 提交中...';
        
        await api.reviews.add(reviewData);
        
        alert('评价提交成功！');
        form.reset();
        
        // 重新加载评价列表
        loadProductReviews(productId, 'product-reviews');
    } catch (error) {
        console.error('提交评价失败:', error);
        alert(`提交评价失败: ${error.message}`);
    } finally {
        const submitButton = document.querySelector(`#${formId} button[type="submit"]`);
        if (submitButton) {
            submitButton.disabled = false;
            submitButton.innerHTML = '提交评价';
        }
    }
}

// 显示编辑评价的模态框
async function showEditReviewModal(reviewId) {
    try {
        // 获取评价详情
        const reviews = await api.reviews.getByUser();
        const review = reviews.data.find(r => r.reviewId == reviewId);
        
        if (!review) {
            alert('找不到该评价');
            return;
        }
        
        // 创建模态框
        const modal = document.createElement('div');
        modal.classList.add('modal', 'fade');
        modal.id = 'editReviewModal';
        modal.setAttribute('tabindex', '-1');
        modal.setAttribute('aria-labelledby', 'editReviewModalLabel');
        modal.setAttribute('aria-hidden', 'true');
        
        modal.innerHTML = `
            <div class="modal-dialog">
                <div class="modal-content">
                    <div class="modal-header">
                        <h5 class="modal-title" id="editReviewModalLabel">编辑评价</h5>
                        <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                    </div>
                    <div class="modal-body">
                        <form id="edit-review-form">
                            <div class="mb-3">
                                <label class="form-label">产品名称</label>
                                <input type="text" class="form-control" value="${review.productName}" disabled>
                            </div>
                            <div class="mb-3">
                                <label class="form-label">评分</label>
                                <div class="rating-input">
                                    <div class="form-check form-check-inline">
                                        <input class="form-check-input" type="radio" name="rating" id="rating1" value="1" ${review.rating === 1 ? 'checked' : ''}>
                                        <label class="form-check-label" for="rating1">1分</label>
                                    </div>
                                    <div class="form-check form-check-inline">
                                        <input class="form-check-input" type="radio" name="rating" id="rating2" value="2" ${review.rating === 2 ? 'checked' : ''}>
                                        <label class="form-check-label" for="rating2">2分</label>
                                    </div>
                                    <div class="form-check form-check-inline">
                                        <input class="form-check-input" type="radio" name="rating" id="rating3" value="3" ${review.rating === 3 ? 'checked' : ''}>
                                        <label class="form-check-label" for="rating3">3分</label>
                                    </div>
                                    <div class="form-check form-check-inline">
                                        <input class="form-check-input" type="radio" name="rating" id="rating4" value="4" ${review.rating === 4 ? 'checked' : ''}>
                                        <label class="form-check-label" for="rating4">4分</label>
                                    </div>
                                    <div class="form-check form-check-inline">
                                        <input class="form-check-input" type="radio" name="rating" id="rating5" value="5" ${review.rating === 5 ? 'checked' : ''}>
                                        <label class="form-check-label" for="rating5">5分</label>
                                    </div>
                                </div>
                            </div>
                            <div class="mb-3">
                                <label for="content" class="form-label">评价内容</label>
                                <textarea class="form-control" id="content" name="content" rows="3" required>${review.content}</textarea>
                            </div>
                            <div class="form-check mb-3">
                                <input class="form-check-input" type="checkbox" id="isAnonymous" name="isAnonymous" ${review.isAnonymous ? 'checked' : ''}>
                                <label class="form-check-label" for="isAnonymous">匿名评价</label>
                            </div>
                        </form>
                    </div>
                    <div class="modal-footer">
                        <button type="button" class="btn btn-secondary" data-bs-dismiss="modal">取消</button>
                        <button type="button" class="btn btn-primary" id="save-review">保存修改</button>
                    </div>
                </div>
            </div>
        `;
        
        document.body.appendChild(modal);
        
        // 显示模态框
        const modalInstance = new bootstrap.Modal(modal);
        modalInstance.show();
        
        // 保存修改
        document.getElementById('save-review').addEventListener('click', async () => {
            try {
                const form = document.getElementById('edit-review-form');
                const rating = parseInt(form.querySelector('input[name="rating"]:checked').value);
                const content = form.querySelector('textarea[name="content"]').value.trim();
                const isAnonymous = form.querySelector('input[name="isAnonymous"]').checked;
                
                if (!rating || !content) {
                    alert('请输入评分和评价内容');
                    return;
                }
                
                const reviewData = {
                    rating,
                    content,
                    isAnonymous
                };
                
                await api.reviews.update(reviewId, reviewData);
                
                alert('评价修改成功！');
                modalInstance.hide();
                
                // 重新加载评价列表
                loadMyReviews('my-reviews');
            } catch (error) {
                console.error('修改评价失败:', error);
                alert(`修改评价失败: ${error.message}`);
            }
        });
        
        // 模态框关闭时移除
        modal.addEventListener('hidden.bs.modal', function () {
            modal.remove();
        });
    } catch (error) {
        console.error('显示编辑评价模态框失败:', error);
        alert(`操作失败: ${error.message}`);
    }
}

// 删除评价
async function deleteReview(reviewId) {
    try {
        await api.reviews.delete(reviewId);
        alert('评价已删除');
        
        // 重新加载评价列表
        loadMyReviews('my-reviews');
    } catch (error) {
        console.error('删除评价失败:', error);
        alert(`删除评价失败: ${error.message}`);
    }
}

// 全局导出
window.reviewsModule = {
    loadProductReviews,
    loadMyReviews,
    submitReview,
    renderStarRating
}; 
 
 